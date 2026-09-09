import Foundation
import StoreKit

private let productId = "palabravo_remove_ads"
public typealias StoreCallback = @convention(c) (Int, UnsafePointer<CChar>?) -> Void

@_cdecl("palabravo_store_listen")
public func storeListen(_ callback: @escaping StoreCallback) {
    Task { @MainActor in
        for await result in Transaction.updates {
            if case .verified(let transaction) = result, transaction.productID == productId {
                String(transaction.id).withCString { callback(0, $0) }
            }
        }
    }
}

@_cdecl("palabravo_store_request")
public func storeRequest(_ operation: UnsafePointer<CChar>, _ argument: UnsafePointer<CChar>, _ context: Int, _ callback: @escaping StoreCallback) {
    let op = String(cString: operation)
    let arg = String(cString: argument)
    Task { @MainActor in
        var response: [String: Any] = [:]
        do {
            switch op {
            case "product":
                if let product = try await Product.products(for: [productId]).first {
                    response = ["id": product.id, "price": product.displayPrice,
                                "currency": product.priceFormatStyle.currencyCode,
                                "amount": NSDecimalNumber(decimal: product.price)]
                }
            case "purchase":
                guard let product = try await Product.products(for: [productId]).first else { throw StoreError.unavailable }
                switch try await product.purchase() {
                case .success(let result):
                    guard case .verified(let transaction) = result else { throw StoreError.unverified }
                    response = ["status": "pending", "proof": proof(transaction, result.jwsRepresentation)]
                case .pending: response = ["status": "pending"]
                case .userCancelled: response = ["status": "cancelled"]
                @unknown default: response = ["status": "failed"]
                }
            case "restore", "current":
                if op == "restore" { try await AppStore.sync() }
                var proofs: [[String: String]] = []
                for await result in Transaction.currentEntitlements {
                    if case .verified(let transaction) = result, transaction.productID == productId {
                        proofs.append(proof(transaction, result.jwsRepresentation))
                    }
                }
                // Include unfinished transactions, so interrupted delivery is retried.
                for await result in Transaction.unfinished {
                    if case .verified(let transaction) = result, transaction.productID == productId,
                       !proofs.contains(where: { $0["transactionId"] == String(transaction.id) }) {
                        proofs.append(proof(transaction, result.jwsRepresentation))
                    }
                }
                response = ["proofs": proofs]
            case "finish":
                for await result in Transaction.unfinished {
                    if case .verified(let transaction) = result, String(transaction.id) == arg,
                       transaction.productID == productId { await transaction.finish() }
                }
                response = ["finished": true]
            default: throw StoreError.unavailable
            }
        } catch { response = ["error": "store_unavailable"] }
        let data = (try? JSONSerialization.data(withJSONObject: response)) ?? Data("{}".utf8)
        String(decoding: data, as: UTF8.self).withCString { callback(context, $0) }
    }
}

private func proof(_ transaction: Transaction, _ jws: String) -> [String: String] {
    ["platform": "ios", "transactionId": String(transaction.id), "proof": jws]
}
private enum StoreError: Error { case unavailable, unverified }
